package org.rtd.rtdnotificationservice.repository;

import org.rtd.rtdnotificationservice.entity.Mail;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface MailRepository extends JpaRepository<Mail,Long> {
}
